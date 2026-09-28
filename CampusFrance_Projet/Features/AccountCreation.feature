Feature: Campus France account registration

  Scenario Outline: Successfully register a researcher account with valid information

    Given user is on the Campus France account creation page

    When user fills in the common fields with user fills in fields "<email>", "<mot_de_passe>", "<confirmation_mot_de_passe>"

    And user fills in "<civilite>", "<nom>", "<prenom>", "<pays_residence>", "<nationalite>", "<code_postal>", "<ville>" and "<telephone>"

    And user selects the "<type_profil>"

    And user fills in field "<domaine>", "<niveau_etudes>", "<fonction>", "<organisme_type>", "<organisme_nom>"

    Then The bouton name is "<button>"

  Examples:
    | civilite | nom     | prenom | pays_residence | nationalite | code_postal | ville | telephone  | type_profil    | email                       | mot_de_passe | confirmation_mot_de_passe | domaine      | niveau_etudes | fonction                              | organisme_type | organisme_nom        | button            |
    | Mr       | Lemaire | Eric   | France         | Francaise   | 75000       | Paris | 0612345678 | Chercheur      | Lemaire.Eric@example.com   | Test@123456  | Test@123456               | Informatique | Doctorat      |                                       |                |                      | Créer un compte |
    | Mme      | Dupont  | Amélie | France         | Algérienne  | 75000       | Paris | 0612345678 | Institutionnel | Dupont.Amelie@example.com  | Test@123456  | Test@123456               |              |               | Responsable des relations internationales | Université     | Université de Paris  | Créer un compte |